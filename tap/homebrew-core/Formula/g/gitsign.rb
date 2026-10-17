class Gitsign < Formula
  desc "Keyless Git signing using Sigstore"
  homepage "https://github.com/sigstore/gitsign"
  url "https://github.com/sigstore/gitsign/archive/refs/tags/v0.17.1.tar.gz"
  sha256 "0325dc76ec9e2d8d81d96aaeb6dfde0317f6cdfc60710fbab9eb636648388085"
  license "Apache-2.0"
  head "https://github.com/sigstore/gitsign.git", branch: "main"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "82f829faa2e2d9eb76dfb36a9ffd003a4650c5e241dab242cc8b359fe6a25f62"
  end

  depends_on "go" => :build

  def install
    ldflags = %W[-X github.com/sigstore/gitsign/pkg/version.gitVersion=#{version}]
    system "go", "build", *std_go_args(ldflags:)
    system "go", "build", *std_go_args(ldflags:, output: bin/"gitsign-credential-cache"),
      "./cmd/gitsign-credential-cache"

    generate_completions_from_executable(bin/"gitsign", shell_parameter_format: :cobra)
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/gitsign --version")

    system "git", "clone", "https://github.com/sigstore/gitsign.git"
    cd testpath/"gitsign" do
      require "pty"
      stdout, _stdin, _pid = PTY.spawn("#{bin}/gitsign attest")
      assert_match "Generating ephemeral keys...", stdout.readline
    end
  end
end
