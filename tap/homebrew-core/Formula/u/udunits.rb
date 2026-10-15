class Udunits < Formula
  desc "Unidata unit conversion library"
  homepage "https://docs.unidata.ucar.edu/udunits/current/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-udunits-2.2.28-2-any.pkg.tar.zst"
  sha256 "b9da2cfbb50bfcd0b147e9178b62f3a7183d848df332160148b44ad9ceac9ea8"
  license "UCAR"

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://downloads.unidata.ucar.edu/udunits/release_info.json"
    strategy :json do |json|
      json["releases"]&.map { |item| item["version"] }
    end
  end

  resource "mingw-w64-ucrt-x86_64-expat" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-expat-2.9.0-1-any.pkg.tar.zst"
    sha256 "f52364dc874009316e1ee1fe46ee89784594b80d10cbcc1cc856945d575308e0"
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/udunits2.exe", "udunits2"
  end

  test do
    assert_match(/1 kg = 1000 g/, shell_output("#{bin}/udunits2 -H kg -W g"))
  end
end
