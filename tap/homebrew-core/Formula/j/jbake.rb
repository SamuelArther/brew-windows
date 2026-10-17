class Jbake < Formula
  desc "Java based static site/blog generator"
  homepage "https://jbake.org/"
  url "https://github.com/jbake-org/jbake/releases/download/v2.7.0/jbake-2.7.0-bin.zip"
  sha256 "dc602d6ebe12d99d33263d0c1cff7f0878c986906dbe6eddf2ca9a89ab14c013"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "6e1079042779590848e9b9288d3748fb20e2256d75a9f4261ed68e5b5d076bc0"
  end

  depends_on "openjdk"

  def install
    rm(Dir["bin/*.bat"])
    libexec.install Dir["*"]
    bin.install libexec/"bin/jbake"
    bin.env_script_all_files libexec/"bin", JAVA_HOME: formula_opt_prefix("openjdk")
  end

  test do
    assert_match "Usage: jbake", shell_output(bin/"jbake")
  end
end
