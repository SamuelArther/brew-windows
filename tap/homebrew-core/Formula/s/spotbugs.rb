class Spotbugs < Formula
  desc "Tool for Java static analysis (FindBugs's successor)"
  homepage "https://spotbugs.github.io/"
  url "https://github.com/spotbugs/spotbugs/releases/download/4.10.5/spotbugs-4.10.5.tgz"
  sha256 "cce3244900cc215ccd9f5a8db29ed9468fc47824faf662f4fad005343d96007e"
  license "LGPL-2.1-or-later"

  bottle do
    sha256 cellar: :any, x86_64_windows: "8dc73316305e5812c243ca996b951915eab1baf2974217a59df40c020f0202be"
  end

  head do
    url "https://github.com/spotbugs/spotbugs.git", branch: "master"

    depends_on "gradle" => :build
  end

  depends_on "openjdk"

  conflicts_with "fb-client", because: "both install a `fb` binary"

  deny_network_access!

  def install
    ENV["JAVA_HOME"] = formula_opt_prefix("openjdk")
    if build.head?
      system "gradle", "build"
      system "gradle", "installDist"
      libexec.install Dir["spotbugs/build/install/spotbugs/*"]
    else
      libexec.install Dir["*"]
      chmod 0755, "#{libexec}/bin/spotbugs"
    end
    (bin/"spotbugs").write_env_script "#{libexec}/bin/spotbugs", Language::Java.overridable_java_home_env
  end

  test do
    (testpath/"HelloWorld.java").write <<~JAVA
      public class HelloWorld {
        private double[] myList;
        public static void main(String[] args) {
          System.out.println("Hello World");
        }
        public double[] getList() {
          return myList;
        }
      }
    JAVA
    system formula_opt_bin("openjdk")/"javac", "HelloWorld.java"
    system formula_opt_bin("openjdk")/"jar", "cvfe", "HelloWorld.jar", "HelloWorld", "HelloWorld.class"
    output = shell_output("#{bin}/spotbugs -textui HelloWorld.jar")
    assert_match(/M V EI.*\nM C UwF.*\n/, output)
  end
end
