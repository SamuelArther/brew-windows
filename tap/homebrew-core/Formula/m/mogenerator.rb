class Mogenerator < Formula
  desc "Generate Objective-C & Swift classes from your Core Data model"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.2/wintools-1.0.2.zip"
  sha256 "02a77df45114a8984fa643e351b9f8dfa5770746f08c6a808a594e486d0b5625"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "mogenerator", "mogenerator"
  end

  test do
    assert_match "mogenerator 1.32", shell_output("#{bin}/mogenerator --version")
    (testpath/"Model.xcdatamodel").mkpath
    (testpath/"Model.xcdatamodel/contents").write <<~XML
      <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
      <model type="com.apple.IDECoreDataModeler.DataModel" documentVersion="1.0">
        <entity name="Person" representedClassName="Person" syncable="YES">
          <attribute name="name" optional="YES" attributeType="String"/>
          <attribute name="age" optional="YES" attributeType="Integer 16" usesScalarValueType="YES"/>
        </entity>
      </model>
    XML
    system bin/"mogenerator", "--model", testpath/"Model.xcdatamodel", "--output-dir", testpath/"out"
    assert_match "@interface _Person : NSManagedObject", (testpath/"out/_Person.h").read
    assert_match "- (int16_t)ageValue", (testpath/"out/_Person.m").read
    assert_path_exists testpath/"out/Person.m"
    system bin/"mogenerator", "--swift", "--model", testpath/"Model.xcdatamodel", "--output-dir", testpath/"swift"
    assert_match "var name: String?", (testpath/"swift/_Person.swift").read
  end
end
