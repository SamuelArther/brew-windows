class Chainsaw < Formula
  desc "Rapidly Search and Hunt through Windows Forensic Artefacts"
  homepage "https://github.com/WithSecureLabs/chainsaw"
  url "https://github.com/WithSecureOpenSource/chainsaw/releases/download/v2.17.1/chainsaw_x86_64-pc-windows-msvc.zip"
  sha256 "1eafd1f4461b61692d62b50e8970c9986798e730396f4069678d25dee14a90d4"
  license "GPL-3.0-only"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/WithSecureLabs/chainsaw/archive/refs/tags/v2.17.1.tar.gz"
  end

  def install
    install_tree
    windows_exe "libexec/chainsaw.exe", "chainsaw"
  end

  test do
    mkdir "chainsaw" do
      output = shell_output("#{bin}/chainsaw lint --kind chainsaw . 2>&1")
      assert_match "Validated 0 detection rules out of 0", output

      output = shell_output("#{bin}/chainsaw dump --json . 2>&1", 1)
      assert_match "Dumping the contents of forensic artefact", output
    end

    assert_match version.to_s, shell_output("#{bin}/chainsaw --version")
  end
end
