class Bttf < Formula
  desc "CLI tool for datetime arithmetic, parsing, formatting and more"
  homepage "https://github.com/BurntSushi/bttf"
  url "https://github.com/BurntSushi/bttf/releases/download/0.1.4/bttf-0.1.4-x86_64-pc-windows-gnu.zip"
  sha256 "b6dc8f7a4c90d3115b4caf91dae216a12272cda5c755e68428d5eb1467029817"
  license any_of: ["Unlicense", "MIT"]

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/BurntSushi/bttf/archive/refs/tags/0.1.4.tar.gz"
  end

  def install
    bin.install "bttf.exe"
  end

  test do
    assert_path_exists bin/"bttf.exe"
  end
end
