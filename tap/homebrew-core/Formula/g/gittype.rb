class Gittype < Formula
  desc "CLI code-typing game that turns your source code into typing challenges"
  homepage "https://github.com/unhappychoice/gittype"
  url "https://github.com/unhappychoice/gittype/releases/download/v0.10.3/gittype-v0.10.3-x86_64-pc-windows-msvc.zip"
  sha256 "53719839d14f9cf7f1329b342b1e2b396ec5cca27c2e97092283e013221a8c85"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/unhappychoice/gittype/archive/refs/tags/v0.10.3.tar.gz"
  end

  def install
    bin.install "gittype.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/gittype --version")

    %w[history stats export].each do |cmd|
      output = shell_output("#{bin}/gittype #{cmd} 2>&1", 1)
      assert_match "command is not yet implemented", output
    end

    output = shell_output("#{bin}/gittype repo list 2>&1", 1)
    assert_match "Error: Terminal error: Not running in a terminal environment", output
  end
end
