class AttemptCli < Formula
  desc "CLI for retrying fallible commands"
  homepage "https://github.com/MaxBondABE/attempt"
  url "https://github.com/MaxBondABE/attempt/archive/refs/tags/v1.1.1.tar.gz"
  sha256 "95c0d7873db135361f5de50e1add0aa472f543dd499aec3cd1fda678672c850a"
  license "Unlicense"
  head "https://github.com/MaxBondABE/attempt.git", branch: "main"

  bottle do
    sha256 cellar: :any, x86_64_windows: "3f9b52db7b2c6bfb3865518f6b48bc5ef63fd96d05d08be69f3f9a4cbcd97e2f"
  end

  depends_on "rust" => :build

  deny_network_access!

  def fetch
    system "cargo", "fetch", *std_cargo_fetch_args
  end

  def install
    system "cargo", "install", *std_cargo_args
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/attempt --version")

    output = shell_output("#{bin}/attempt fixed -a 1 -m 0s -- sh -c 'echo ok'")
    assert_match "ok", output
  end
end
