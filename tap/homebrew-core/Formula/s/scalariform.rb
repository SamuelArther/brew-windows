class Scalariform < Formula
  desc "Scala source code formatter"
  homepage "https://github.com/scala-ide/scalariform"
  url "https://github.com/scala-ide/scalariform/releases/download/0.2.10/scalariform.jar"
  sha256 "59d7c26f26c13bdbc27e3011da244f01001d55741058062f49e4626862b7991e"
  license "MIT"

  livecheck do
    url :stable
    strategy :github_latest
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "c4f496b383f347a100786f35d9449280c18ff957e7b9b709c63290c212cc8924"
  end

  head do
    url "https://github.com/scala-ide/scalariform.git", branch: "master"
    depends_on "sbt" => :build
  end

  depends_on "openjdk"

  def install
    if build.head?
      system "sbt", "project cli", "assembly"
      libexec.install Dir["cli/target/scala-*/cli-assembly-*.jar"]
      bin.write_jar_script Dir[libexec/"cli-assembly-*.jar"][0], "scalariform"
    else
      libexec.install "scalariform.jar"
      bin.write_jar_script libexec/"scalariform.jar", "scalariform"
    end
  end

  test do
    before_data = <<~SCALA
      def foo() {
      println("Hello World")
      }
    SCALA

    after_data = <<~SCALA
      def foo() {
         println("Hello World")
      }
    SCALA

    (testpath/"foo.scala").write before_data
    system bin/"scalariform", "-indentSpaces=3", testpath/"foo.scala"
    assert_equal after_data, (testpath/"foo.scala").read
  end
end
