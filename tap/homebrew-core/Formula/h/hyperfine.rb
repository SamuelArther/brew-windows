class Hyperfine < Formula
  desc "Command-line benchmarking tool"
  homepage "https://github.com/sharkdp/hyperfine"
  url "https://github.com/sharkdp/hyperfine/releases/download/v2.0.0/hyperfine-v2.0.0-x86_64-pc-windows-msvc.zip"
  sha256 "3dec51acee1060908e67948838d6bc0a7aacde0774acebfa4afb220dcf0bc078"
  license any_of: ["Apache-2.0", "MIT"]

  livecheck do
    url :stable
    strategy :github_latest
  end

  def install
    install_tree
    windows_exe "libexec/hyperfine.exe", "hyperfine"
  end

  test do
    assert_match "hyperfine #{version}", shell_output("#{bin}/hyperfine --version")
  end
end
