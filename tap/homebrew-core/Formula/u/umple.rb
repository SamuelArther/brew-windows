class Umple < Formula
  desc "Modeling tool/programming language that enables Model-Oriented Programming"
  homepage "https://cruise.umple.org/umple/"
  url "https://github.com/umple/umple/releases/download/v1.38.0/umple-1.38.0.8918.bda708970.jar"
  sha256 "64a4c87bf83c8a977f7e84511a400673590d9b648b4e08567e1a86f0f944598e"
  license "MIT"
  version_scheme 1

  livecheck do
    url :stable
    strategy :github_latest
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "718cf858e5861baaf5a8c4147e79b09bc1cc3c2121ad97c49d03d7cc0f43e621"
  end

  no_autobump! because: :incompatible_version_format

  depends_on "openjdk"

  def install
    filename = File.basename(stable.url)

    libexec.install filename
    bin.write_jar_script libexec/filename, "umple"
  end

  test do
    (testpath/"test.ump").write("class X{ a; }")
    system bin/"umple", "test.ump", "-c", "-"
    assert_path_exists testpath/"X.java"
    assert_path_exists testpath/"X.class"
  end
end
