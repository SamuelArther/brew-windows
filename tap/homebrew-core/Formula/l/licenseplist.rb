class Licenseplist < Formula
  desc "License list generator of all your dependencies for iOS applications"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.3/wintools-1.0.3.zip"
  sha256 "82a71bdf0b54d1c31bb140791ad3ef7b7c4f7bea1394de5697c78d96758281ea"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "license-plist", "license-plist"
  end

  test do
    (testpath/"Cartfile.resolved").write <<~EOS
      github "realm/realm-swift" "v10.20.2"
    EOS
    assert_match "None", shell_output("#{bin}/license-plist --suppress-opening-directory")
    (testpath/"license_plist.yml").write <<~YAML
      manual:
        - name: FooLib
          body: 'Copyright (c) Foo'
    YAML
    system bin/"license-plist", "--suppress-opening-directory", "--force", "--cartfile-path", "none"
    assert_match "com.mono0926.LicensePlist/FooLib", (testpath/"com.mono0926.LicensePlist.Output/com.mono0926.LicensePlist.plist").read
    assert_match "Copyright (c) Foo", (testpath/"com.mono0926.LicensePlist.Output/com.mono0926.LicensePlist/FooLib.plist").read
  end
end
