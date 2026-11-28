import { TablePagination } from '@mui/material'

type Props = {
  count: number
  page: number
  pageSize: number
  onPageChange: (page: number) => void
  onPageSizeChange: (pageSize: number) => void
}

export function ClientPaginationBar({ count, page, pageSize, onPageChange, onPageSizeChange }: Props) {
  const safePage = count === 0 ? 0 : Math.min(page, Math.max(0, Math.ceil(count / pageSize) - 1))
  return <TablePagination component="div" count={count} page={safePage} rowsPerPage={pageSize}
    rowsPerPageOptions={[10, 20, 50]} labelRowsPerPage="Số dòng mỗi trang:"
    labelDisplayedRows={({ from, to, count: total }) => `${from}–${to} / ${total}`}
    onPageChange={(_, nextPage) => onPageChange(nextPage)}
    onRowsPerPageChange={(event) => onPageSizeChange(Number(event.target.value))} />
}
