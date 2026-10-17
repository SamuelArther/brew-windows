class Whistle < Formula
  desc "HTTP, HTTP2, HTTPS, Websocket debugging proxy"
  homepage "https://wproxy.org/"
  url "https://registry.npmjs.org/whistle/-/whistle-2.10.11.tgz"
  sha256 "22f9a5380c541feeecddf02821675b4d6c026fcf4e2af536b02a7b8262b5c02e"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "60cf5997bb7db7ae8ef6a9ef0ce7c6684cde90399aac6a87d85b458cce47a14c"
  end

  depends_on "node"

  def install
    system "npm", "install", *std_npm_args
    bin.install_symlink libexec.glob("bin/*")
  end

  test do
    (testpath/"package.json").write('{"name": "test"}')
    system bin/"whistle", "start"
    system bin/"whistle", "stop"
  end
end
