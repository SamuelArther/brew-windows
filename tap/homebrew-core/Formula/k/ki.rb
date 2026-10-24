class Ki < Formula
  desc "Kotlin Language Interactive Shell"
  homepage "https://github.com/Kotlin/kotlin-interactive-shell"
  url "https://github.com/Kotlin/kotlin-interactive-shell/archive/refs/tags/v0.6.0.tar.gz"
  sha256 "46404602ca69c18a777ed2c88de5c5eec7e43d4cc1e77a4b44e413701bd327a0"
  license "Apache-2.0"
  head "https://github.com/Kotlin/kotlin-interactive-shell.git", branch: "main"

  bottle do
    sha256 cellar: :any, x86_64_windows: "e776dfc7269792974c420e26bb4aa80cfc747b9d6437cebbd70c2684806e4c48"
  end

  depends_on "maven" => :build
  depends_on "openjdk"

  allow_network_access! :build

  def install
    ENV["JAVA_HOME"] = Language::Java.java_home

    system "mvn", "-DskipTests", "package"
    libexec.install "lib/ki-shell.jar", "lib/ki-completion-plugin.jar"
    bin.write_jar_script libexec/"ki-shell.jar", "ki"
  end

  test do
    output = pipe_output(bin/"ki", ":q")
    assert_match "ki-shell", output
    assert_match "Bye!", output
  end
end
