class Gfxutil < Formula
  desc "Device Properties conversion tool"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.2/wintools-1.0.2.zip"
  sha256 "02a77df45114a8984fa643e351b9f8dfa5770746f08c6a808a594e486d0b5625"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "gfxutil", "gfxutil"
  end

  test do
    assert_equal "02010c00d041030a000000000101060000007fff0400",
      shell_output("#{bin}/gfxutil -o hex -c \"PciRoot(0x0)/Pci(0x0,0x0)\"")
    assert_match "PciRoot(0x0)/Pci(0x1f,0x3)",
      shell_output("#{bin}/gfxutil -c 02010c00d041030a0000000001010600031f7fff0400")

    (testpath/"props.plist").write <<~XML
      <?xml version="1.0" encoding="UTF-8"?>
      <plist version="1.0">
      <dict>
        <key>PciRoot(0x0)/Pci(0x2,0x0)</key>
        <dict>
          <key>AAPL,ig-platform-id</key>
          <data>AwBmAQ==</data>
          <key>model</key>
          <string>Intel HD 4000</string>
        </dict>
      </dict>
      </plist>
    XML
    system bin/"gfxutil", "-i", "xml", "-o", "hex", testpath/"props.plist", testpath/"props.hex"
    assert_match(/\A[0-9a-f]+\z/, (testpath/"props.hex").read)
    xml = shell_output("#{bin}/gfxutil #{testpath}/props.hex")
    assert_match "<data>AwBmAQ==</data>", xml
    assert_match "<string>Intel HD 4000</string>", xml
  end
end
