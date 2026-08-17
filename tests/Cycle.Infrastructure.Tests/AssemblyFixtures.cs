using Cycle.Tests.Common;

// Bootstrap MSBuild once before any test in this assembly runs. Class fixtures
// alone are not enough: collections run in parallel, so a test that touches
// MSBuild types without the fixture would race the locator registration.
[assembly: AssemblyFixture(typeof(MsBuildFixture))]
