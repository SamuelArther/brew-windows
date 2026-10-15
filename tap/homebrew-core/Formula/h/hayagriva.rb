class Hayagriva < Formula
  desc "Bibliography management tool"
  homepage "https://github.com/typst/hayagriva"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-hayagriva-0.10.1-2-any.pkg.tar.zst"
  sha256 "54b2e658f4fc324ed278cfc60443f2e0b0a6f7d26edc30026cc211689e360a66"
  license any_of: ["Apache-2.0", "MIT"]

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://github.com/typst/hayagriva/archive/refs/tags/v0.10.1.tar.gz"
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/hayagriva.exe", "hayagriva"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/hayagriva --version")

    (testpath/"test.yaml").write <<~YAML
      dependence:
          type: Article
          title: The program dependence graph and its use in optimization
          author: ["Ferrante, Jeanne", "Ottenstein, Karl J.", "Warren, Joe D."]
          date: 1987-07
          serial-number:
              doi: "10.1145/24039.24041"
          parent:
              type: Periodical
              title: ACM Transactions on Programming Languages and Systems
              volume: 9
              issue: 3
    YAML

    output = "Ferrante, J., Ottenstein, K. J., & Warren, J. D. (1987)."
    assert_match output, shell_output("#{bin}/hayagriva test.yaml reference --no-fmt --style apa")
  end
end
