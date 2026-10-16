class Qdl < Formula
  desc "Flash firmware on Qualcomm-based devices via USB Emergency Download (EDL) mode"
  homepage "https://github.com/linux-msm/qdl"
  url "https://github.com/linux-msm/qdl/releases/download/v2.8/qdl-binary-windows-x64-v2.8.zip"
  sha256 "f013b0a38660957feb5959ee5c625013089e9a1f9a9f5e7f21eb0cbd1cdee159"
  license "BSD-3-Clause"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/linux-msm/qdl/archive/refs/tags/v2.8.tar.gz"
  end

  def install
    install_tree
    windows_exe "libexec/qdl.exe", "qdl"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/qdl --version")

    (testpath/"prog_firehose_ddr.elf").write "\0" * 1024
    (testpath/"efi.bin").write "\0" * 8192
    (testpath/"rawprogram0.xml").write <<~XML
      <?xml version="1.0" ?>
      <data>
        <program SECTOR_SIZE_IN_BYTES="4096" file_sector_offset="0" filename="efi.bin" label="efi" num_partition_sectors="2" physical_partition_number="0" size_in_KB="8.0" sparse="false" start_byte_hex="0x6000" start_sector="6"/>
      </data>
    XML

    (testpath/"vip").mkpath
    system bin/"qdl", "--dry-run", "--create-digests=#{testpath}/vip",
           "prog_firehose_ddr.elf", "rawprogram0.xml"
    assert_path_exists testpath/"vip/DigestsToSign.bin"
    assert_predicate (testpath/"vip/DigestsToSign.bin").size, :positive?
  end
end
