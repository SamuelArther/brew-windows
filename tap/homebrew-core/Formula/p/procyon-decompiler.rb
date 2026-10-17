class ProcyonDecompiler < Formula
  desc "Modern decompiler for Java 5 and beyond"
  homepage "https://github.com/mstrobel/procyon"
  url "https://github.com/mstrobel/procyon/releases/download/v0.6.0/procyon-decompiler-0.6.0.jar"
  sha256 "821da96012fc69244fa1ea298c90455ee4e021434bc796d3b9546ab24601b779"
  license "Apache-2.0"

  bottle do
    sha256 cellar: :any, x86_64_windows: "bcce9da9bed69d508abb2efba17c920770fe5d3455b100f40048a70e65e71225"
  end

  depends_on "openjdk@21"

  def install
    libexec.install "procyon-decompiler-#{version}.jar"
    bin.write_jar_script libexec/"procyon-decompiler-#{version}.jar", "procyon-decompiler", java_version: "21"
  end

  test do
    fixture = <<~JAVA
      class T
      {
          public static void main(final String[] array) {
              System.out.println("Hello World!");
          }
      }
    JAVA

    (testpath/"T.java").write fixture
    system formula_opt_bin("openjdk@21")/"javac", "T.java"
    assert_match fixture, shell_output("#{bin}/procyon-decompiler T.class")
  end
end
