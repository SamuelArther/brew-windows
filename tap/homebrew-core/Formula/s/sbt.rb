class Sbt < Formula
  desc "Build tool for Scala projects"
  homepage "https://www.scala-sbt.org/"
  url "https://github.com/sbt/sbt/releases/download/v2.0.10/sbt-2.0.10.zip"
  sha256 "6ff28e1dea38d1de48ea3d4e903f0380b85b19f945a103e30ec4298717c90cd7"
  license "Apache-2.0"

  livecheck do
    url :stable
    strategy :github_latest
  end

  depends_on "openjdk"

  # Official sbt launcher zip; sbt.bat uses JAVA_HOME or the java on PATH (openjdk).

  def install
    install_tree
    windows_cmd "libexec/bin/sbt.bat", "sbt"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/sbt --script-version")
  end
end
