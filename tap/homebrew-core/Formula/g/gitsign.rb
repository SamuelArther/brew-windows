class Gitsign < Formula
  desc "Keyless Git signing using Sigstore"
  homepage "https://github.com/sigstore/gitsign"
  url "https://github.com/sigstore/gitsign/archive/refs/tags/v0.18.0.tar.gz"
  sha256 "d87dbb00624d0e6874ef2df37b3bb3b3a89416c3a19114753d3c3cb932dcdcba"
  license "Apache-2.0"
  head "https://github.com/sigstore/gitsign.git", branch: "main"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "149dda6b1fe43292e666f862d4639eabe7475bea5af9edc56913e27825f3178b"
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
    assert_match version.to_s, shell_output("#{bin}/gitsign version")

    system "git", "clone", "https://github.com/sigstore/gitsign.git"
    cd testpath/"gitsign" do
      require "pty"
      stdout, _stdin, _pid = PTY.spawn("#{bin}/gitsign attest")
      assert_match "Generating ephemeral keys...", stdout.readline
    end
  end
end
