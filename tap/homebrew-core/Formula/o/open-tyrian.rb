class OpenTyrian < Formula
  desc "Open-source port of Tyrian"
  homepage "https://github.com/opentyrian/opentyrian"
  url "https://github.com/opentyrian/opentyrian/releases/download/v2.1.20260913/opentyrian-v2.1.20260913-windows-x86_64.zip"
  sha256 "7eddbe42439392ee8b2ffbe38c1c20a768da0c4afcc795ca31928c9dd6068f60"
  license "GPL-2.0-or-later"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/opentyrian/opentyrian/archive/refs/tags/v2.1.20260913.tar.gz"
  end

  def install
    install_tree
    windows_exe "libexec/opentyrian.exe", "opentyrian"
  end

  test do
    assert_path_exists bin/"opentyrian.exe"
  end
end
