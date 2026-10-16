class Leetsolv < Formula
  desc "CLI tool for DSA problem revision with spaced repetition"
  homepage "https://github.com/eannchen/leetsolv"
  url "https://github.com/eannchen/leetsolv/releases/download/v1.2.0/leetsolv-windows-amd64.exe"
  sha256 "c99167a421f4b18daf08800d57df2c07f8e6830b132528020cec2f904c8a7f63"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/eannchen/leetsolv/archive/refs/tags/v1.2.0.tar.gz"
  end

  def install
    bin.install "leetsolv-windows-amd64.exe" => "leetsolv.exe"
  end

  test do
    output = shell_output("#{bin}/leetsolv status")
    assert_match "Total Questions: 0", output

    input = "test note\n5\n1\n2\n"
    pipe_output("#{bin}/leetsolv add https://leetcode.com/problems/two-sum", input)

    output = shell_output("#{bin}/leetsolv status")
    assert_match "Total Questions: 1", output
  end
end
