class Pinact < Formula
  desc "Pins GitHub Actions to full hashes and versions"
  homepage "https://github.com/suzuki-shunsuke/pinact"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-pinact-5.0.0-1-any.pkg.tar.zst"
  sha256 "ee6d8e5a3f27d688b77864aa8b679a0992f9526a40a67d95971a7320cccddedc"
  license "MIT"

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://github.com/suzuki-shunsuke/pinact/archive/refs/tags/v5.0.0.tar.gz"
    regex(/^v?(\d+(?:\.\d+)+)$/i)
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/pinact.exe", "pinact"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/pinact --version")

    (testpath/"action.yml").write <<~YAML
      name: CI

      on: push

      jobs:
        build:
          runs-on: ubuntu-latest
          steps:
            - uses: actions/checkout@v3
            - run: npm install && npm test
    YAML

    system bin/"pinact", "run", "action.yml"

    assert_match(%r{.*?actions/checkout@[a-f0-9]{40}}, (testpath/"action.yml").read)
  end
end
