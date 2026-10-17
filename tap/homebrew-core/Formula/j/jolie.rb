class Jolie < Formula
  desc "Service-oriented programming language"
  homepage "https://www.jolie-lang.org/"
  url "https://github.com/jolie/jolie/releases/download/v1.13.4/jolie-1.13.4.jar"
  sha256 "fd374b75b29c55f6c073b1cd29e6087ae48f99cd1541d0417010cd86629bd9c9"
  license "LGPL-2.1-only"

  bottle do
    sha256 cellar: :any, x86_64_windows: "f47f5590921fd9fb9975d0ce821a6992fc0f9fe8dbd7fd4aec579cfabefff7c1"
  end

  depends_on "openjdk"

  def install
    system formula_opt_bin("openjdk")/"java",
    "-jar", "jolie-#{version}.jar",
    "--jolie-home", libexec,
    "--jolie-launchers", libexec/"bin"
    bin.install libexec.glob("bin/*")
    bin.env_script_all_files libexec/"bin",
      JOLIE_HOME: "${JOLIE_HOME:-#{libexec}}",
      JAVA_HOME:  "${JAVA_HOME:-#{formula_opt_prefix("openjdk")}}"
  end

  test do
    file = testpath/"test.ol"
    file.write <<~EOS
      from console import Console, ConsoleIface

      interface PowTwoInterface { OneWay: powTwo( int ) }

      service main(){

        outputPort Console { interfaces: ConsoleIface }
        embed Console in Console

        inputPort In {
          location: "local://testPort"
          interfaces: PowTwoInterface
        }

        outputPort Self {
          location: "local://testPort"
          interfaces: PowTwoInterface
        }

        init {
          powTwo@Self( 4 )
        }

        main {
          powTwo( x )
          println@Console( x * x )()
        }

      }
    EOS

    out = shell_output("#{bin}/jolie #{file}").strip

    assert_equal "16", out
  end
end
