class Folderify < Formula
  desc "Generate folder icons with an engraved mask image in the Windows folder style"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.3/wintools-1.0.3.zip"
  sha256 "82a71bdf0b54d1c31bb140791ad3ef7b7c4f7bea1394de5697c78d96758281ea"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "folderify", "folderify"
  end

  test do
    (testpath/"test.svg").write <<~EOS
      <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100">
        <circle cx="50" cy="50" r="40" fill="transparent" stroke="black" stroke-width="20" />
      </svg>
    EOS
    system bin/"folderify", "test.svg", "--output-iconset", testpath/"test.iconset", "--no-progress"
    assert_predicate testpath/"test.iconset/icon_512x512.png", :size?
    system bin/"folderify", test_fixtures("test.png"), "--output-ico", testpath/"test.ico", "--badge", "locked"
    assert_equal "\x00\x00\x01\x00".b, (testpath/"test.ico").binread[0, 4]
    (testpath/"folder").mkpath
    system bin/"folderify", "test.svg", testpath/"folder"
    assert_path_exists testpath/"folder/desktop.ini"
    assert_match "Windows folder style", shell_output("#{bin}/folderify --help")
  end
end
