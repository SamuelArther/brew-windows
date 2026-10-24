class Qqqa < Formula
  desc "Fast, stateless LLM for your shell: qq answers; qa runs commands"
  homepage "https://github.com/iagooar/qqqa"
  url "https://github.com/iagooar/qqqa/archive/refs/tags/v1.0.1.tar.gz"
  sha256 "1a47852ade8175570c3b0f6358a2850be4281ffa9511e14cc4240420e4d02a75"
  license "MIT"
  head "https://github.com/iagooar/qqqa.git", branch: "master"

  bottle do
    sha256 cellar: :any, x86_64_windows: "6adda4d993e86f0b196833ee6b5ea80bf1902258f433ec2cccea73d0f103e433"
  end

  depends_on "rust" => :build

  allow_network_access! :test

  def fetch
    # `Cargo.lock` is ignored
    system "cargo", "generate-lockfile"
    system "cargo", "fetch", *std_cargo_fetch_args
  end

  def install
    system "cargo", "install", *std_cargo_args
  end

  test do
    %w[qq qa].each do |cmd|
      assert_match version.to_s, shell_output("#{bin}/#{cmd} --version")
    end

    assert_match "Error: Missing API key", shell_output("#{bin}/qq 'test' 2>&1", 1)
  end
end
