class Xcp < Formula
  desc "Fast & lightweight command-line tool for managing Xcode projects"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.2/wintools-1.0.2.zip"
  sha256 "02a77df45114a8984fa643e351b9f8dfa5770746f08c6a808a594e486d0b5625"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "xcp", "xcp"
  end

  test do
    assert_match "1.2.3", shell_output("#{bin}/xcp --version")
    assert_match "Error: The project doesn't contain a .pbxproj file at path:",
                 shell_output("#{bin}/xcp list-targets 2>&1", 1)

    (testpath/"App.xcodeproj").mkpath
    (testpath/"App.xcodeproj/project.pbxproj").write <<~EOS
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
    (testpath/"App").mkpath
    (testpath/"App/Model.swift").write "struct Model {}\n"
    assert_equal "App\n", shell_output("#{bin}/xcp list-targets")
    system bin/"xcp", "add-file", "--file", "App/Model.swift", "--targets", "App"
    assert_equal "App\n", shell_output("#{bin}/xcp list-targets --file App/Model.swift")
    assert_match "Model.swift in Sources", (testpath/"App.xcodeproj/project.pbxproj").read
    system bin/"xcp", "set-build-setting", "--key", "SWIFT_VERSION", "--value", "5.0"
    assert_equal "5.0\n", shell_output("#{bin}/xcp get-build-setting --target App --key SWIFT_VERSION")
  end
end
