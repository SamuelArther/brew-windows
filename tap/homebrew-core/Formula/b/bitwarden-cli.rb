class BitwardenCli < Formula
  desc "Secure and free password manager for all of your devices"
  homepage "https://bitwarden.com/"
  url "https://github.com/bitwarden/clients/archive/refs/tags/cli-v2026.10.0.tar.gz"
  sha256 "a400f7042710daafa7d64ed04461a331c09a8bbb7262c68ce870ff3cbc336ea2"
  license "GPL-3.0-only"
  head "https://github.com/bitwarden/clients.git", branch: "main"

  livecheck do
    url :stable
    regex(/^cli[._-]v?(\d+(?:\.\d+)+)$/i)
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "072e4f4c3dd125beb238d224575be0cc4f0afc8fad23a013fc7977df7be07a57"
  end

  depends_on "node"

  def install
    system "npm", "install", *std_npm_args(prefix: false)
    cd buildpath/"apps/cli" do
      # The `oss` build of Bitwarden is a GPL backed build
      system "npm", "run", "build:oss:prod", "--ignore-scripts"
      system "npm", "install", *std_npm_args
    end
    bin.install_symlink libexec.glob("bin/*")

    generate_completions_from_executable(bin/"bw", "completion", "--shell", shells: [:zsh])
  end

  test do
    assert_equal 10, shell_output("#{bin}/bw generate --length 10").length

    output = pipe_output("#{bin}/bw encode", "Testing", 0)
    assert_equal "VGVzdGluZw==", output
  end
end
