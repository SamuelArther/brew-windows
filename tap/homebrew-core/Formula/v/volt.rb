class Volt < Formula
  desc "Meta-level vim package manager"
  homepage "https://github.com/vim-volt/volt"
  url "https://github.com/vim-volt/volt/releases/download/v0.3.7/volt-v0.3.7-windows-amd64.exe"
  sha256 "6a2bb7dc0c71a1ff1f0f18126a8898c82ffab6002c2e19e0ccd194d74dc54641"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/vim-volt/volt/archive/refs/tags/v0.3.7.tar.gz"
  end

  def install
    bin.install "volt-v0.3.7-windows-amd64.exe" => "volt.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/volt version 2>&1")
  end
end
