class AiCli < Formula
  desc "Generate images, video, audio, and text from the terminal"
  homepage "https://ai-cli.dev"
  url "https://registry.npmjs.org/ai-cli/-/ai-cli-0.7.0.tgz"
  sha256 "d51b19346e33fe5825a7a46fbd20dbb0fa7f210addaa763704f785d738d2c99a"
  license "Apache-2.0"

  bottle do
    sha256 cellar: :any, x86_64_windows: "ba53c1fce4d7f0bb9d1657308c836cdacf5e44f1b5d04da4718d101e7df2c4c4"
  end

  depends_on "node"

  deny_network_access! [:postinstall, :test]

  def install
    system "npm", "install", *std_npm_args
    bin.install_symlink libexec.glob("bin/*")
  end

  test do
    output = shell_output("#{bin}/ai text --image #{testpath/"missing.png"} describe 2>&1", 1)
    assert_match "could not read reference image", output
  end
end
