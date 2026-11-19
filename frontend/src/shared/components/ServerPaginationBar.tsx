import { FormControl, MenuItem, Pagination, Paper, Select, Stack, Typography } from '@mui/material'

interface ServerPaginationBarProps {
  totalCount: number
  page: number
  pageSize: number
  totalPages: number
  onPageChange: (page: number) => void
  onPageSizeChange: (pageSize: number) => void
  pageSizeOptions?: number[]
  itemLabel?: string
}

export function ServerPaginationBar({
  totalCount,
  page,
  pageSize,
  totalPages,
  onPageChange,
  onPageSizeChange,
  pageSizeOptions = [5, 10, 20, 50],
  itemLabel = 'kết quả',
}: ServerPaginationBarProps) {
  const safeTotalPages = Math.max(0, totalPages)
  const currentPage = safeTotalPages === 0 ? 1 : Math.min(Math.max(1, page), safeTotalPages)
  const start = totalCount === 0 ? 0 : (currentPage - 1) * pageSize + 1
  const end = totalCount === 0 ? 0 : Math.min(totalCount, currentPage * pageSize)

  return (
    <Paper
      variant="outlined"
      sx={{
        px: 2,
        py: 1.5,
        borderRadius: 2.5,
        display: 'flex',
        flexDirection: { xs: 'column', sm: 'row' },
        alignItems: { xs: 'stretch', sm: 'center' },
        justifyContent: 'space-between',
        gap: 1.5,
      }}
    >
      <Typography variant="body2" color="text.secondary">
        Hiển thị <b>{start}–{end}</b> / tổng <b>{totalCount.toLocaleString('vi-VN')}</b> {itemLabel} · Trang{' '}
        <b>{safeTotalPages === 0 ? 0 : currentPage}</b>/{safeTotalPages}
      </Typography>

      <Stack direction="row" spacing={1.5} alignItems="center" justifyContent="space-between" flexWrap="wrap" useFlexGap>
        <Stack direction="row" spacing={1} alignItems="center">
          <Typography variant="caption" color="text.secondary">
            Mỗi trang:
          </Typography>
          <FormControl size="small" sx={{ minWidth: 74 }}>
            <Select
              value={pageSize}
              inputProps={{ 'aria-label': 'Số kết quả mỗi trang' }}
              onChange={(e) => onPageSizeChange(Number(e.target.value))}
            >
              {pageSizeOptions.map((option) => (
                <MenuItem key={option} value={option}>
                  {option}
                </MenuItem>
              ))}
            </Select>
          </FormControl>
        </Stack>

        <Pagination
          size="small"
          color="primary"
          showFirstButton
          showLastButton
          count={Math.max(1, safeTotalPages)}
          page={currentPage}
          disabled={safeTotalPages <= 1}
          onChange={(_, nextPage) => onPageChange(nextPage)}
        />
      </Stack>
    </Paper>
  )
}
