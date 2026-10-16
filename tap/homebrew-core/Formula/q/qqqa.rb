class Qqqa < Formula
  desc "Fast, stateless LLM for your shell: qq answers; qa runs commands"
  homepage "https://github.com/iagooar/qqqa"
  url "https://github.com/iagooar/qqqa/archive/refs/tags/v1.0.1.tar.gz"
  sha256 "1a47852ade8175570c3b0f6358a2850be4281ffa9511e14cc4240420e4d02a75"
  license "MIT"
  head "https://github.com/iagooar/qqqa.git", branch: "master"

  bottle do
    sha256 cellar: :any, x86_64_windows: "6247364993ec0ab89f637effcf94b86d94c9fc240b2fe678a4ca6e6f985e521a"
  end

  depends_on "rust" => :build

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
