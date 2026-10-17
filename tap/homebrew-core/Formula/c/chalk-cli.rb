class ChalkCli < Formula
  desc "Terminal string styling done right"
  homepage "https://github.com/chalk/chalk-cli"
  url "https://registry.npmjs.org/chalk-cli/-/chalk-cli-6.0.0.tgz"
  sha256 "480a85e48da024092e1b63fe260f810880f5f82322d82f62304f32e970112216"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "87ead7b308698d0f8153594debac7b8e05ebf9966f9a629a6d9aa8117130e864"
  end

  depends_on "node"

  def install
    system "npm", "install", *std_npm_args
    bin.install_symlink libexec.glob("bin/*")
  end

  test do
    assert_match "hello, world!", pipe_output("#{bin}/chalk bold cyan --stdin", "hello, world!")
  end
end
