class Define < Formula
  desc "Command-line dictionary (thesaurus) app, with access to multiple sources"
  homepage "https://github.com/Rican7/define"
  url "https://github.com/Rican7/define/releases/download/v0.4.0/define_windows_amd64.exe"
  sha256 "79420f7fe82871d0f6d4b4a4243b7404511c09330b8e47afdcce0bbbfa9897e3"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/Rican7/define/archive/refs/tags/v0.4.0.tar.gz"
  end

  def install
    bin.install "define_windows_amd64.exe" => "define.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/define --version 2>&1")
  end
end
