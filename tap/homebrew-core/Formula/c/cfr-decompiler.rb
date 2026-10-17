class CfrDecompiler < Formula
  desc "Yet Another Java Decompiler"
  homepage "https://www.benf.org/other/cfr/"
  url "https://github.com/leibnitz27/cfr.git",
      tag:      "0.152",
      revision: "68477be3ff7171ee17ddd1a26064b9b253f1604f"
  license "MIT"
  head "https://github.com/leibnitz27/cfr.git", branch: "master"

  livecheck do
    url :homepage
    regex(/href=.*?cfr[._-]v?(\d+(?:\.\d+)+)\.jar/i)
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "205c362f91b47690a21d716d7eac9b97d18cc93679e47b26f0dd0f982a7cfbcd"
  end

  depends_on "maven" => :build
  depends_on "openjdk@21"

  def install
    ENV["JAVA_HOME"] = formula_opt_prefix("openjdk@21")
    # changing the compiler because 6 is used by upstream and openjdk no longer supports it
    system formula_opt_bin("maven")/"mvn", "package", "-Dmaven.compiler.source=8", "-Dmaven.compiler.target=8"

    cd "target" do
      if build.head?
        lib_jar = Dir["cfr-*-SNAPSHOT.jar"]
        doc_jar = Dir["cfr-*-SNAPSHOT-javadoc.jar"]
        odie "Unexpected number of artifacts!" if (lib_jar.length != 1) || (doc_jar.length != 1)
        lib_jar = lib_jar[0]
        doc_jar = doc_jar[0]
      else
        lib_jar = "cfr-#{version}.jar"
        doc_jar = "cfr-#{version}-javadoc.jar"
      end

      # install library and binary
      libexec.install lib_jar
      bin.write_jar_script libexec/lib_jar, "cfr-decompiler", java_version: "21"

      # install library docs
      doc.install doc_jar
      mkdir doc/"javadoc"
      cd doc/"javadoc" do
        system formula_opt_bin("openjdk@21")/"jar", "-xf", doc/doc_jar
        rm_r("META-INF")
      end
    end
  end

  test do
    fixture = <<~JAVA
      /*
       * Decompiled with CFR #{version}.
       */
      class T {
          T() {
          }

          public static void main(String[] stringArray) {
              System.out.println("Hello brew!");
          }
      }
    JAVA
    (testpath/"T.java").write fixture
    system formula_opt_bin("openjdk@21")/"javac", "T.java"
    output = pipe_output("#{bin}/cfr-decompiler --comments false T.class")
    assert_match fixture, output
  end
end
