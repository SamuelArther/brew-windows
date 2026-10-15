class Scala < Formula
  desc "JVM-based programming language"
  homepage "https://scala-lang.org/"
  url "https://github.com/scala/scala3/releases/download/3.10.0/scala3-3.10.0-x86_64-pc-win32.zip"
  sha256 "28f58a614bdcce1554b7d9a2acd697c0fd88839b6bfbd4e0420e98d641a58472"
  license "Apache-2.0"

  livecheck do
    url :stable
    strategy :github_latest
  end

  depends_on "openjdk"

  # Official Scala 3 Windows distribution; it bundles the scala-cli runner that
  # homebrew-core ships as a separate scala-cli dependency.

  def install
    install_tree
    windows_cmd "libexec/bin/scala.bat", "scala"
    windows_cmd "libexec/bin/scalac.bat", "scalac"
    windows_cmd "libexec/bin/scaladoc.bat", "scaladoc"
  end

  test do
    file = testpath/"Test.scala"
    file.write <<~SCALA
      object Test {
        def main(args: Array[String]): Unit = {
          println(s"${2 + 2}")
        }
      }
    SCALA

    out = shell_output("#{bin}/scala #{file}").strip
    assert_equal "4", out
  end
end
