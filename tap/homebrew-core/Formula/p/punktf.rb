class Punktf < Formula
  desc "Cross-platform multi-target dotfiles manager"
  homepage "https://shemnei.github.io/punktf/"
  url "https://github.com/Shemnei/punktf/releases/download/v3.1.2/punktf-x86_64-pc-windows-msvc-static.zip"
  sha256 "406594f17a79e7c726d4ff6319a406e2054fe976d6b96f1ca50496f758b8dd8c"
  license any_of: ["Apache-2.0", "MIT"]

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/Shemnei/punktf/archive/refs/tags/v3.1.2.tar.gz"
  end

  def install
    bin.install "punktf.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/punktf --version")

    mkdir_p testpath/"profiles"
    touch testpath/"profiles/test.yaml"

    output = shell_output("#{bin}/punktf deploy --profile test --source #{testpath} --target #{testpath}")
    assert_match "SUCCESS", output
  end
end
