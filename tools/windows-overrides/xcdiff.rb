class Xcdiff < Formula
  desc "Tool to diff xcodeproj files"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.2/wintools-1.0.2.zip"
  sha256 "02a77df45114a8984fa643e351b9f8dfa5770746f08c6a808a594e486d0b5625"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "xcdiff", "xcdiff"
  end

  test do
    assert_match "0.14.0", shell_output("#{bin}/xcdiff --version")
    pbxproj = <<~EOS
      // !$*UTF8*$!
      {
      	archiveVersion = 1;
      	classes = {
      	};
      	objectVersion = 56;
      	objects = {
      		A1 = {isa = PBXBuildFile; fileRef = F1; };
      		F1 = {isa = PBXFileReference; lastKnownFileType = sourcecode.swift; path = App.swift; sourceTree = "<group>"; };
      		G0 = {isa = PBXGroup; children = (G1, ); sourceTree = "<group>"; };
      		G1 = {isa = PBXGroup; children = (F1, ); path = App; sourceTree = "<group>"; };
      		S1 = {isa = PBXSourcesBuildPhase; buildActionMask = 2147483647; files = (A1, ); runOnlyForDeploymentPostprocessing = 0; };
      		T1 = {isa = PBXNativeTarget; buildConfigurationList = L2; buildPhases = (S1, ); dependencies = (); name = App; productName = App; productType = "com.apple.product-type.application"; };
      		C1 = {isa = XCBuildConfiguration; buildSettings = {}; name = Debug; };
      		C2 = {isa = XCBuildConfiguration; buildSettings = {PRODUCT_NAME = App; }; name = Debug; };
      		L1 = {isa = XCConfigurationList; buildConfigurations = (C1, ); };
      		L2 = {isa = XCConfigurationList; buildConfigurations = (C2, ); };
      		P1 = {isa = PBXProject; buildConfigurationList = L1; mainGroup = G0; targets = (T1, ); };
      	};
      	rootObject = P1;
      }
    EOS
    (testpath/"One.xcodeproj").mkpath
    (testpath/"Two.xcodeproj").mkpath
    (testpath/"One.xcodeproj/project.pbxproj").write pbxproj
    (testpath/"Two.xcodeproj/project.pbxproj").write pbxproj.gsub("App.swift", "Main.swift")
    assert_equal "\n", shell_output("#{bin}/xcdiff -p1 One.xcodeproj -p2 One.xcodeproj -d")
    output = shell_output("#{bin}/xcdiff -p1 One.xcodeproj -p2 Two.xcodeproj -g SOURCES -v", 2)
    assert_match "SOURCES > \"App\" target", output
    assert_match "App/App.swift", output
    assert_match "App/Main.swift", output
  end
end
