using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace aspnetcore_PolymorphicBinding.ViewModels
{
	public class ModelOne : IPolymorphicModel
	{
		[Range(1, 10)]
		public int				PropOne { get; set; }
	}

	public class ModelTwo : IPolymorphicModel
	{
		[Range(1, 10)]
		public int				PropTwo { get; set; }
	}

	public class TestPolymorphicModels
	{
		public PolymorphicModels<ModelOne, ModelTwo>		Tester { get; set; }

		public TestPolymorphicModels()
		{
			Tester = new PolymorphicModels<ModelOne, ModelTwo>(new ModelOne(), new ModelTwo());
		}
	}
}
