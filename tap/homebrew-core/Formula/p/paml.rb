class Paml < Formula
  desc "Phylogenetic analyses of DNA or protein sequences using maximum likelihood"
  homepage "https://github.com/abacus-gene/paml"
  url "https://github.com/abacus-gene/paml/releases/download/v4.10.10/paml-4.10.10-win-x86_64.tar.gz"
  sha256 "1de2cd4821391a9b5e32be1699d3afb87c2dc78104275c4dedcb0b3d64a1bdca"
  license "GPL-3.0-only"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/abacus-gene/paml/archive/refs/tags/v4.10.10.tar.gz"
    regex(/^v?(\d+(?:\.\d+)+)$/i)
  end

  def install
    install_tree
    windows_exe "libexec/bin/baseml.exe", "baseml"
    windows_exe "libexec/bin/basemlg.exe", "basemlg"
    windows_exe "libexec/bin/chi2.exe", "chi2"
    windows_exe "libexec/bin/codeml.exe", "codeml"
    windows_exe "libexec/bin/evolver.exe", "evolver"
    windows_exe "libexec/bin/infinitesites.exe", "infinitesites"
    windows_exe "libexec/bin/mcmctree.exe", "mcmctree"
    windows_exe "libexec/bin/pamp.exe", "pamp"
    windows_exe "libexec/bin/yn00.exe", "yn00"
  end

  test do
    assert_path_exists bin/"baseml.exe"
  end
end
