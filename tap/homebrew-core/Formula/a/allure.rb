class Allure < Formula
  desc "Flexible lightweight test report tool"
  homepage "https://allurereport.org/"
  url "https://repo.maven.apache.org/maven2/io/qameta/allure/allure-commandline/2.46.2/allure-commandline-2.46.2.zip"
  sha256 "43da085ce226c239c886d2818e4eec7be2a9a09ac0f3f42a6b092cdb27be696e"
  license "Apache-2.0"

  livecheck do
    url "https://search.maven.org/remotecontent?filepath=io/qameta/allure/allure-commandline/maven-metadata.xml"
    regex(%r{<version>v?(\d+(?:\.\d+)+)</version>}i)
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "4e9fef9c49bd43649d848dd7125911026e499c98907543ae8b3526a56359bf87"
  end

  depends_on "openjdk"

  deny_network_access!

  def install
    # Remove all windows files
    rm(Dir["bin/*.bat"])

    libexec.install Dir["*"]
    bin.install libexec.glob("bin/*")
    bin.env_script_all_files libexec/"bin", JAVA_HOME: formula_opt_prefix("openjdk")
  end

  test do
    (testpath/"allure-results/allure-result.json").write <<~JSON
      {
        "uuid": "allure",
        "name": "testReportGeneration",
        "fullName": "org.homebrew.AllureFormula.testReportGeneration",
        "status": "passed",
        "stage": "finished",
        "start": 1494857300486,
        "stop": 1494857300492,
        "labels": [
          {
            "name": "package",
            "value": "org.homebrew"
          },
          {
            "name": "testClass",
            "value": "AllureFormula"
          },
          {
            "name": "testMethod",
            "value": "testReportGeneration"
          }
        ]
      }
    JSON
    system bin/"allure", "generate", testpath/"allure-results", "-o", testpath/"allure-report"
  end
end
