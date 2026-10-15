class Zizmor < Formula
  desc "Find security issues in GitHub Actions setups"
  homepage "https://docs.zizmor.sh/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-zizmor-1.30.1-1-any.pkg.tar.zst"
  sha256 "4569507d7fa1545fa10708b4f1d9db0e2b53fdc7ecab010cc1abc6d6afc0550d"
  license "MIT"

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://github.com/zizmorcore/zizmor/archive/refs/tags/v1.31.0.tar.gz"
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/zizmor.exe", "zizmor"
  end

  test do
    (testpath/"workflow.yaml").write <<~YAML
      on: push
      jobs:
        vulnerable:
          runs-on: ubuntu-latest
          steps:
            - name: Checkout
              uses: actions/checkout@v4
    YAML

    output = shell_output("#{bin}/zizmor --format plain #{testpath}/workflow.yaml", 14)
    assert_match "does not set persist-credentials: false", output
  end
end
