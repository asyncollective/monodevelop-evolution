﻿//
// VsTestTestClass.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2017 Xamarin Inc. (http://xamarin.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System.Collections.Generic;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using MonoDevelop.Core.Execution;
using MonoDevelop.Projects;
using MonoDevelop.UnitTesting;
using MonoDevelop.Ide;
using System;
using MonoDevelop.Ide.TypeSystem;
using System.Linq;

namespace MonoDevelop.UnitTesting.VsTest
{
	class VsTestTestClass : UnitTestGroup, IVsTestTestProvider
	{
		public Project Project { get; private set; }
		IVsTestTestRunner testRunner;
		SourceCodeLocation sourceCodeLocation;

		public VsTestTestClass (IVsTestTestRunner testRunner, Project project, VsTestUnitTest vsTestUnit)
			: base (vsTestUnit.FixtureTypeName)
		{
			this.Project = project;
			this.testRunner = testRunner;
			FixtureTypeName = vsTestUnit.FixtureTypeName;
			TestSourceCodeDocumentId = string.IsNullOrEmpty (vsTestUnit.FixtureTypeNamespace) ? FixtureTypeName : vsTestUnit.FixtureTypeNamespace + "." + FixtureTypeName;

			if (sourceCodeLocation == null) {
				IdeApp.TypeSystemService.GetCompilationAsync (Project).ContinueWith ((t) => {
					var className = TestSourceCodeDocumentId;

					var compilation = t.Result;
					if (compilation == null)
						return;
					var cls = compilation.GetTypeByMetadataName (className);
					if (cls == null)
						return;
					var source = cls.Locations.FirstOrDefault (l => l.IsInSource);
					if (source == null)
						return;
					var line = source.GetLineSpan ();

					sourceCodeLocation = new SourceCodeLocation (source.SourceTree.FilePath, line.StartLinePosition.Line, line.StartLinePosition.Character);
				}).Ignore();
			}
		}

		protected override UnitTestResult OnRun (TestContext testContext)
		{
			return testRunner.RunTest (testContext, this).Result;
		}

		protected override bool OnCanRun (IExecutionHandler executionContext)
		{
			return testRunner.CanRunTests (executionContext);
		}

		public override bool HasTests {
			get { return true; }
		}

		public IEnumerable<TestCase> GetTests ()
		{
			foreach (IVsTestTestProvider testProvider in Tests) {
				foreach (TestCase childTest in testProvider.GetTests ()) {
					yield return childTest;
				}
			}
		}

		public override SourceCodeLocation SourceCodeLocation {
			get {
				return sourceCodeLocation;
			}
		}

	}
}
