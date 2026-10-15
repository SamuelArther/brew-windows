class Wildmidi < Formula
  desc "Simple software midi player"
  homepage "https://github.com/Mindwerks/wildmidi"
  url "https://github.com/Mindwerks/wildmidi/releases/download/wildmidi-0.5.0/wildmidi-0.5.0-windows.zip"
  sha256 "4a8c5b7cd2868c4fb9443e0f56de7dd37a41119ac3c1031a0bf90991e8d8e47d"
  license all_of: ["GPL-3.0-only", "LGPL-3.0-only"]

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/Mindwerks/wildmidi/archive/refs/tags/wildmidi-0.5.0.tar.gz"
  end

  def install
    install_tree
    windows_exe "libexec/arm64/wildmidi.exe", "wildmidi"
  end

  test do
    assert_path_exists bin/"wildmidi.exe"
  end
end
