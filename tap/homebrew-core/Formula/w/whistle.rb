class Whistle < Formula
  desc "HTTP, HTTP2, HTTPS, Websocket debugging proxy"
  homepage "https://wproxy.org/"
  url "https://registry.npmjs.org/whistle/-/whistle-2.10.13.tgz"
  sha256 "4f2f284640f2d0174d504f0a9be61c4eebe25d9c8b5c7b54a431a627bfbdf7f7"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "efa3c606a8ccc397e0923233f52c99dec7e5ee235e6caf6cb9414243f831465a"
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
