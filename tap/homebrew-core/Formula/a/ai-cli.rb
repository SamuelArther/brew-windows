class AiCli < Formula
  desc "Generate images, video, audio, and text from the terminal"
  homepage "https://ai-cli.dev"
  url "https://registry.npmjs.org/ai-cli/-/ai-cli-0.8.0.tgz"
  sha256 "3198b937055bb07acff469818a6366f61bd461a03a3ce01bc1ce1d9d1c5a6a7e"
  license "Apache-2.0"

  bottle do
    sha256 cellar: :any, x86_64_windows: "2a71b94b75dd9fd90ec0f099014c534db12d3695dbbbb5e3e1d6aad75a7d03a5"
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
