class SwiftSection < Formula
  desc "CLI tool for parsing Mach-O, PE and ELF files to obtain Swift information"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.2/wintools-1.0.2.zip"
  sha256 "02a77df45114a8984fa643e351b9f8dfa5770746f08c6a808a594e486d0b5625"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "swift-section", "swift-section"
  end

  test do
    # A Mach-O dylib with the Swift metadata swiftc emits for module Test's
    #   public struct MyTestStruct { public let id: Int; public var name: String }
    # a __swift5_types record, the module and struct context descriptors, the field descriptor and
    # its strings, all linked by 32-bit relative pointers.
    dylib = [0xfeedfacf, 0x01000007, 3, 6, 1, 232, 0, 0,
             0x19, 232, "__TEXT", 0, 0x1000, 0, 0x290, 5, 5, 2, 0,
             "__swift5_types", "__TEXT", 0x200, 4, 0x200, 2, 0, 0, 0, 0, 0, 0,
             "__const", "__TEXT", 0x204, 0x8c, 0x204, 2, 0, 0, 0, 0, 0, 0,
             0x20,
             0, 0, 0x58,
             0x51, -0x14, 0x4d, 0, 0x10, 2, 2,
             0, 0, 0, 12, 2,
             0, 0x2e, 0x2d, 2, 0x28, 0x27,
             "Test\0MyTestStruct\0Si\0id\0SS\0name\0"]
    (testpath/"libTest.dylib").binwrite dylib.pack("V8V2a16Q<4V4a16a16Q<2V8a16a16Q<2V8x248Vx12V3x4V7x4V2v2VV6x8a*")
    system bin/"swift-section", "dump", "libTest.dylib", "-o", "output.txt", "-s", "types"
    assert_match "struct Test.MyTestStruct", (testpath/"output.txt").read
    assert_match "var name: Swift.String", (testpath/"output.txt").read
    assert_match "dump", shell_output("#{bin}/swift-section --help")
  end
end
