class Yuicompressor < Formula
  desc "Yahoo! JavaScript and CSS compressor"
  homepage "https://yui.github.io/yuicompressor/"
  url "https://github.com/yui/yuicompressor/releases/download/v2.4.8/yuicompressor-2.4.8.zip"
  sha256 "3243fd79cb68cc61a5278a8ff67a0ad6a2d825c36464594b66900ad8426a6a6e"
  license "BSD-3-Clause"
  revision 1

  livecheck do
    url :stable
    strategy :github_latest
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "539149458bbf97309922c8ad3945fae3091adedd7bec8d309ce7b2eb58106d58"
  end

  depends_on "openjdk"

  def install
    libexec.install "yuicompressor-#{version}.jar"
    bin.write_jar_script libexec/"yuicompressor-#{version}.jar", "yuicompressor"
  end

  test do
    path = testpath/"test.js"
    path.write <<~JAVASCRIPT
      var i = 1;      // foo
      console.log(i); // bar
    JAVASCRIPT

    output = shell_output("#{bin}/yuicompressor --nomunge --preserve-semi #{path}").strip
    assert_equal "var i=1;console.log(i);", output
  end
end
