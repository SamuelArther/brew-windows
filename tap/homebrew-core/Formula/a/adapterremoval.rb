class Adapterremoval < Formula
  desc "Rapid adapter trimming, identification, and read merging"
  homepage "https://github.com/MikkelSchubert/adapterremoval"
  url "https://github.com/MikkelSchubert/adapterremoval/releases/download/v3.0.2/adapterremoval-v3.0.2-windows-x86_64.zip"
  sha256 "1dca3123700387efc9f59ca5bf03a2321881309f8dba977be93cdb450ddb516b"
  license "GPL-3.0-or-later"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/MikkelSchubert/adapterremoval/archive/refs/tags/v3.0.2.tar.gz"
  end

  def install
    install_tree
    windows_exe "libexec/adapterremoval3.exe", "adapterremoval3"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/adapterremoval3 --version 2>&1")
  end
end
