class Vineflower < Formula
  desc "Java decompiler"
  homepage "https://vineflower.org/"
  url "https://github.com/Vineflower/vineflower/archive/refs/tags/1.12.0.tar.gz"
  sha256 "25c783b5b73fd0ae8c904c9ea3e7ee1e6713812d4d5fd085547fb20f1dacfdfb"
  license "Apache-2.0"

  # Issue ref: https://github.com/Vineflower/vineflower/issues/495
  depends_on "gradle@8" => :build
  depends_on "openjdk"

  bottle do
    sha256 cellar: :any, x86_64_windows: "b995b3d0ded4ec88a6ce616acb043d0482848a2e8dfe27d95c4192f42a36ca3c"
  end

  def install
    system "gradle", "build", "-x", "test"

    version_suffix = ENV["GITHUB_ACTIONS"] ? "" : "+local"
    jar = Dir["build/libs/vineflower-#{version}#{version_suffix}.jar"].first
    libexec.install jar => "vineflower.jar"

    bin.write_jar_script libexec/"vineflower.jar", "vineflower"
  end

  test do
    (testpath/"FooBar.java").write <<~JAVA
      public class FooBar {
        public static void bar() {}
        public static void foo() {
          bar();
        }
      }
    JAVA

    system formula_opt_bin("openjdk")/"javac", "FooBar.java"
    refute_includes shell_output("#{bin}/vineflower FooBar.class"), "error"
  end
end
