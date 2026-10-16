class Wskdeploy < Formula
  desc "Apache OpenWhisk project deployment utility"
  homepage "https://openwhisk.apache.org/"
  url "https://github.com/apache/openwhisk-wskdeploy/releases/download/1.2.0/openwhisk_wskdeploy-1.2.0-windows-amd64.zip"
  sha256 "4eb0b8a2adb671c7bc0a4d5b952cdd90466a2b08ac6692708cfa7bab9e2b4e99"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/apache/openwhisk-wskdeploy/archive/refs/tags/1.2.0.tar.gz"
  end

  def install
    install_tree
    windows_exe "libexec/wskdeploy.exe", "wskdeploy"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/wskdeploy version")

    (testpath/"manifest.yaml").write <<~YAML
      packages:
        hello_world_package:
          version: 1.0
          license: Apache-2.0
    YAML

    system bin/"wskdeploy", "-v",
                            "--apihost", "openwhisk.ng.bluemix.net",
                            "--preview",
                            "-m", testpath/"manifest.yaml",
                            "-u", "abcd"
  end
end
