using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.Extensions.Logging;

namespace aspnetcore_PolymorphicBinding.ViewModels
{
	public interface IPolymorphicModel {}

	public class PolymorphicModelWrapper
	{
		public IPolymorphicModel Model { get; set; }
	}

	public class PolymorphicModelList
	{
		public List<PolymorphicModelWrapper> PolymorphicModels { get; set; } = new List<PolymorphicModelWrapper>();
	}

	public class PolymorphicModels<T1, T2> : PolymorphicModelList
		where T1 : IPolymorphicModel
		where T2 : IPolymorphicModel
	{
		public PolymorphicModels() {}

		public PolymorphicModels(T1 model1, T2 model2)
		{
			PolymorphicModels.Add(new PolymorphicModelWrapper() { Model = model1} );
			PolymorphicModels.Add(new PolymorphicModelWrapper() { Model = model2} );
		}
	}



	public class PolymorphicModelBinderProvider : IModelBinderProvider
	{

		/// <inheritdoc />
		public IModelBinder GetBinder(ModelBinderProviderContext context)
		{
			if (context.Metadata.ModelType.IsGenericType && context.Metadata.ModelType.GetGenericTypeDefinition() == typeof(PolymorphicModels<,>))
			{
				var model_types = context.Metadata.ModelType.GenericTypeArguments;
				var polymorphic_submodel_binders = new List<(Type, ModelMetadata, IModelBinder)>();

				foreach (var model_type in model_types)
				{
					var model_metadata = context.MetadataProvider.GetMetadataForType(model_type);
					var model_binder = context.CreateBinder(model_metadata);
					polymorphic_submodel_binders.Add((model_type, model_metadata, model_binder));
				}

				// go through PolymorphicModels<T1,T2> properties
				Dictionary<ModelMetadata, IModelBinder> polymorphic_models_binders = new Dictionary<ModelMetadata, IModelBinder>();
				for (int i = 0; i < context.Metadata.Properties.Count; i++)
				{
					var property = context.Metadata.Properties[i];
					IModelBinder model_binder;
					if (property.ModelType == typeof(List<PolymorphicModelWrapper>))
					{
						var polymorphic_model_wrapper_property_binders = new Dictionary<ModelMetadata, IModelBinder>();
						var polymorphic_model_wrapper_metadata = context.MetadataProvider.GetMetadataForType(typeof(PolymorphicModelWrapper));
						for (int j = 0; j < polymorphic_model_wrapper_metadata.Properties.Count; j++)
						{
							var wrapper_property = polymorphic_model_wrapper_metadata.Properties[j]; 
							IModelBinder wrapper_property_binder;
							if(wrapper_property.ModelType == typeof(IPolymorphicModel))
							{
								wrapper_property_binder = new IPolymorphicModelBinder(polymorphic_submodel_binders);
							}
							else
							{
								wrapper_property_binder = context.CreateBinder(wrapper_property);
							}
							polymorphic_model_wrapper_property_binders.Add(wrapper_property, wrapper_property_binder);
						}
						var polymorphic_model_wrapper_binder = new ComplexTypeModelBinder(polymorphic_model_wrapper_property_binders, context.Services.GetService(typeof(ILoggerFactory)) as ILoggerFactory);
						model_binder = new CollectionModelBinder<PolymorphicModelWrapper>(polymorphic_model_wrapper_binder, context.Services.GetService(typeof(ILoggerFactory)) as ILoggerFactory);
					}
					else
					{
						model_binder = context.CreateBinder(property);
					}

					polymorphic_models_binders.Add(property, model_binder);
				}
				
				return new ComplexTypeModelBinder(polymorphic_models_binders, context.Services.GetService(typeof(ILoggerFactory)) as ILoggerFactory);
			}

			return null;
			
		}
	}

	public class IPolymorphicModelBinder : IModelBinder
	{
		private readonly List<(Type, ModelMetadata, IModelBinder)> _binders;

		public IPolymorphicModelBinder(List<(Type, ModelMetadata, IModelBinder)> binders)
		{
			_binders = binders;
		}

		/// <inheritdoc />
		public async Task BindModelAsync(ModelBindingContext bindingContext)
		{
			if (bindingContext.ModelName.EndsWith("].Model"))
			{
				var indexer_idx = bindingContext.ModelName.LastIndexOf('[');
				string idxs = "";
				while (++indexer_idx < bindingContext.ModelName.Length && bindingContext.ModelName[indexer_idx] != ']')
				{
					idxs += bindingContext.ModelName[indexer_idx];
				}
				int idx;
				if (!int.TryParse(idxs, out idx))
				{
					bindingContext.Result = ModelBindingResult.Failed();
					return;
				}

				if (idx < 0 || idx >= _binders.Count)
				{
					bindingContext.Result = ModelBindingResult.Failed();
					return;
				}

				var newBindingContext = DefaultModelBindingContext.CreateBindingContext(
					bindingContext.ActionContext,
					bindingContext.ValueProvider,
					_binders[idx].Item2,
					bindingInfo: null,
					bindingContext.ModelName);

				await _binders[idx].Item3.BindModelAsync(newBindingContext);
				bindingContext.Result = newBindingContext.Result;

				bindingContext.ValidationState.Add(newBindingContext.Result.Model, new ValidationStateEntry
				{
					Metadata = _binders[idx].Item2,
				});

				return;
			}

			bindingContext.Result = ModelBindingResult.Failed();
			return;
		}
	}

}
