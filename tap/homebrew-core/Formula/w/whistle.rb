class Whistle < Formula
  desc "HTTP, HTTP2, HTTPS, Websocket debugging proxy"
  homepage "https://wproxy.org/"
  url "https://registry.npmjs.org/whistle/-/whistle-2.10.12.tgz"
  sha256 "3f1cc024020ad36f5a81a5d95bb5751d14f059033d8d3a6f7997db3e1fd95967"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "43b5ce9464b8cd9ce666411cebd90fa19dd111094d48038373ea41063630569a"
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
