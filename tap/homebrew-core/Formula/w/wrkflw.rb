class Wrkflw < Formula
  desc "Validate and execute GitHub Actions workflows locally"
  homepage "https://github.com/bahdotsh/wrkflw"
  url "https://github.com/bahdotsh/wrkflw/releases/download/v0.8.0/wrkflw-x86_64-pc-windows-msvc.zip"
  sha256 "4ef968a5164402dd8284fe250a363a0a6d2e66e6c1b34f7e27b4b71a2554f369"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/bahdotsh/wrkflw/archive/refs/tags/v0.8.0.tar.gz"
  end

  def install
    bin.install "wrkflw.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/wrkflw --version")

    test_action_config = testpath/".github/workflows/test.yml"
    test_action_config.write <<~YAML
      name: test

      on: [push]

      jobs:
        test:
          runs-on: ubuntu-latest
          steps:
            - uses: actions/checkout@v4
    YAML

    output = shell_output("#{bin}/wrkflw validate #{test_action_config}")
    assert_match "Validating GitHub workflow file: #{testpath}/.github/workflows/test.yml... ✔ Valid", output
  end
end
