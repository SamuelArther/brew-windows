class Pmd < Formula
  desc "Source code analyzer for Java, JavaScript, and more"
  homepage "https://pmd.github.io"
  url "https://github.com/pmd/pmd/releases/download/pmd_releases%2F7.30.0/pmd-dist-7.30.0-bin.zip"
  sha256 "14fbf34dbcb51e895e27c33898cd6d7e9d028b1cc22ae23a5e8b100f7538446e"
  license "BSD-4-Clause"

  livecheck do
    url :stable
    strategy :github_latest
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "a99dea8b2ecf029ba7292bde3b3fa33f233a15fecef616b340888c6c9cf340b2"
  end

  depends_on "openjdk"

  deny_network_access!

  def install
    rm Dir["bin/*.bat"]
    libexec.install Dir["*"]
    (bin/"pmd").write_env_script libexec/"bin/pmd", Language::Java.overridable_java_home_env
  end

  test do
    (testpath/"java/BrewTestClass.java").write <<~JAVA
      public class BrewTestClass {
        // dummy constant
        public String SOME_CONST = "foo";

        public boolean doTest () {
          return true;
        }
      }
    JAVA

    output = shell_output("#{bin}/pmd check -d #{testpath}/java " \
                          "-R category/java/bestpractices.xml -f json")
    assert_empty JSON.parse(output)["processingErrors"]
  end
end
