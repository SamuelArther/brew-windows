class Sox < Formula
  desc "SOund eXchange: universal sound sample translator"
  homepage "https://sox.sourceforge.net/"
  version "14.4.2"
  url "https://downloads.sourceforge.net/project/sox/sox/14.4.2/sox-14.4.2-win32.zip"
  sha256 "8072cc147cf1a3b3713b8b97d6844bb9389e211ab9e1101e432193fad6ae6662"
  license all_of: ["GPL-2.0-or-later", "LGPL-2.1-or-later"]

  # Official SoX Windows build; upstream only ships a 32-bit (x86) binary.

  def install
    install_tree
    windows_exe "libexec/sox.exe", "sox"
  end

  test do
    output = testpath/"out.wav"
    system bin/"sox", "-n", output, "synth", "0.1", "sine", "440"
    assert_path_exists output
  end
end
