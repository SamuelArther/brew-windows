class Kumo < Formula
  desc "Word Clouds in Java"
  homepage "https://github.com/kennycason/kumo"
  url "https://search.maven.org/remotecontent?filepath=com/kennycason/kumo-cli/1.28/kumo-cli-1.28.jar"
  sha256 "43e4e2ea9da62a2230deed9151d8484f80bd6ae5fef304eaadf3301378f45fb6"
  license "MIT"

  livecheck do
    url "https://search.maven.org/remotecontent?filepath=com/kennycason/kumo-cli/maven-metadata.xml"
    regex(%r{<version>v?(\d+(?:\.\d+)+)</version>}i)
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "d2c1006b1cc7f0a407b7da043afc14c00780a2cd89810959c10588f8df34d839"
  end

  depends_on "openjdk"

  def install
    libexec.install "kumo-cli-#{version}.jar"
    bin.write_jar_script libexec/"kumo-cli-#{version}.jar", "kumo"
  end

  test do
    system bin/"kumo", "-i", "https://brew.sh/", "-o", testpath/"brew.png"
    assert_path_exists testpath/"brew.png", "Wordcloud was not generated!"
  end
end
