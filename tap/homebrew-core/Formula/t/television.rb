class Television < Formula
  desc "General purpose fuzzy finder TUI"
  homepage "https://alexpasmantier.github.io/television/"
  url "https://github.com/alexpasmantier/television/releases/download/0.15.9/tv-0.15.9-x86_64-pc-windows-msvc.tar.gz"
  sha256 "b6ecb3bf2587d8068a50c855671b2b693acaf172fcddb6542b6f80db1e38b681"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/alexpasmantier/television/archive/refs/tags/0.15.9.tar.gz"
  end

  conflicts_with "tidy-viewer", because: "both install `tv` binaries"

  def install
    install_tree
    windows_exe "libexec/tv.exe", "tv"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/tv -V")

    output = shell_output("#{bin}/tv help")
    assert_match "fuzzy finder for the terminal", output
  end
end
